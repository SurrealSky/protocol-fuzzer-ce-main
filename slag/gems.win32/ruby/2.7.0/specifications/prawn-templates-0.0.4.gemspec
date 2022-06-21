# -*- encoding: utf-8 -*-
# stub: prawn-templates 0.0.4 ruby lib

Gem::Specification.new do |s|
  s.name = "prawn-templates".freeze
  s.version = "0.0.4"

  s.required_rubygems_version = Gem::Requirement.new(">= 1.3.6".freeze) if s.respond_to? :required_rubygems_version=
  s.require_paths = ["lib".freeze]
  s.authors = ["Gregory Brown".freeze, "Brad Ediger".freeze, "Daniel Nelson".freeze, "Jonathan Greenberg".freeze, "James Healy".freeze, "Burkhard Vogel".freeze]
  s.date = "2016-04-03"
  s.description = "Prawn::Templates allows using PDFs as templates in Prawn".freeze
  s.email = ["gregory.t.brown@gmail.com".freeze, "brad@bradediger.com".freeze, "dnelson@bluejade.com".freeze, "greenberg@entryway.net".freeze, "jimmy@deefa.com".freeze, "b.vogel@buddyandselly.com".freeze]
  s.homepage = "https://github.com/prawnpdf/prawn-templates".freeze
  s.licenses = ["PRAWN".freeze, "GPL-2.0".freeze, "GPL-3.0".freeze]
  s.required_ruby_version = Gem::Requirement.new(">= 1.9.3".freeze)
  s.rubygems_version = "3.1.6".freeze
  s.summary = "Prawn::Templates allows using PDFs as templates in Prawn".freeze

  s.installed_by_version = "3.1.6" if s.respond_to? :installed_by_version

  if s.respond_to? :specification_version then
    s.specification_version = 4
  end

  if s.respond_to? :add_runtime_dependency then
    s.add_runtime_dependency(%q<pdf-reader>.freeze, ["~> 1.3"])
    s.add_runtime_dependency(%q<prawn>.freeze, [">= 2.1.0"])
    s.add_runtime_dependency(%q<pdf-core>.freeze, [">= 0.5.0"])
    s.add_development_dependency(%q<pdf-inspector>.freeze, ["~> 1.2.0"])
    s.add_development_dependency(%q<rspec>.freeze, [">= 0"])
    s.add_development_dependency(%q<rake>.freeze, [">= 0"])
    s.add_development_dependency(%q<rubocop>.freeze, ["= 0.30.1"])
  else
    s.add_dependency(%q<pdf-reader>.freeze, ["~> 1.3"])
    s.add_dependency(%q<prawn>.freeze, [">= 2.1.0"])
    s.add_dependency(%q<pdf-core>.freeze, [">= 0.5.0"])
    s.add_dependency(%q<pdf-inspector>.freeze, ["~> 1.2.0"])
    s.add_dependency(%q<rspec>.freeze, [">= 0"])
    s.add_dependency(%q<rake>.freeze, [">= 0"])
    s.add_dependency(%q<rubocop>.freeze, ["= 0.30.1"])
  end
end
